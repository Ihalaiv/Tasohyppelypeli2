using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mime;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System.Linq;
using System.Linq.Expressions;
namespace pororun;

/// @author Kaljami
/// @version 03.03.2024
/// <summary>
/// 
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class pororun : PhysicsGame
{
    private const int RUUDUN_KOKO = 40;
    private const double NOPEUS = 7500; //Asettaa nopeuden mitä ukkeli kulkee alussa 
    private double nykyinenNopeus = NOPEUS; // muuttaa nopeutta kokoajan kasvavaksi
    private const double HYPPYNOPEUS = 250; // vakio miten korkealle ukkeli hyppää per napinpainallus
    private PlatformCharacter pelaaja1;
    private Image pelaajanKuva = LoadImage("hahmo.png");
    private Image tahtiKuva = LoadImage("salmiakkia.png");
    private Timer liikutusajastin;
    private Image  vihollinenkuva= LoadImage("vihu.png");
    private SoundEffect maaliAani = LoadSoundEffect("maali.wav");
    private bool peliKaynnissa = false;
    private bool kentta1lapi = false;
    private int kenttaNro = 0;
    private int keratytSalmiakit = 0;
    IntMeter pisteLaskuri;/// asetetaan pistelaskuri, pelin käynnissäolo sekä muita vakioita


     /// <summary>
    /// Aloittaa pelin asettamalla painovoiman, kutsuu ensimmäisen kentän luontia, lisää näppäimet,
    /// luo pistelaskurin, määrittää kameran seuraamaan pelaajaa ja käynnistää pelaajan liikuttamisajastimen.
    /// </summary>
    public override void Begin()
    {
        Gravity = new Vector(0, -800); // asettaa painovoiman
        LuoSeuraavaKentta();
        LisaaNappaimet();
        LuoPistelaskuri();
        Camera.Follow(pelaaja1);
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;

        MasterVolume = 0.5;
        liikutusajastin = new Timer();
        liikutusajastin.Interval = 0.01;
        liikutusajastin.Timeout += SiirraPelaajaaOikeammalle;
        liikutusajastin.Start();
        peliKaynnissa = true;
    }


    /// <summary>
    /// PelaajanNopeus funktio säätää pelaajan tämänhetkinsen nopeuden, sekä kasvattaa tämänhetkistä nopeutta kertoimella
    /// </summary>
    private void PelaajanNopeus()
    {
        nykyinenNopeus *= 1.00005;
    }


    /// <summary>
    /// Pakottaa pelaajan liikkumaan oikealle
    /// </summary>
    private void SiirraPelaajaaOikeammalle()
    {
        PelaajanNopeus(); 
        pelaaja1.Push(new Vector(nykyinenNopeus, 0.0));
    }

    
    /// <summary>
    /// Luo kentän 2 kun kenttä 1 on läpäisty
    /// </summary>
    private void LuoSeuraavaKentta()
    {
        if(kenttaNro==1)
        {
            string levu = "kentta1";
            LuoKentta(levu);   
        }
        else
        {
            string levu = "kentta2";
            LuoKentta(levu);
            liikutusajastin = new Timer();
            liikutusajastin.Interval = 0.01;
            liikutusajastin.Timeout += SiirraPelaajaaOikeammalle;
            liikutusajastin.Start();
            peliKaynnissa = true;
            Camera.Follow(pelaaja1);
        }
    }

    
    /// <summary>
    /// Rakentaa kenttään tason katon esteet ja muut, sekä aseittaa näppäimet
    /// </summary>
    /// <param name="taso">määrittää mikä taso on kyseessä</param>
    private void LuoKentta(string taso)
    {
       ClearAll();
       Camera.Reset();
        TileMap kentta = TileMap.FromLevelAsset(taso);// Muodostaa kentän vastamaan tektitiedostoa. 
        kentta.SetTileMethod('=', LisaaEste);// lisää esteet tekstitiedoston mukaan
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('*', LisaaTahti);
        kentta.SetTileMethod('J', LisaaPelaaja);
        kentta.SetTileMethod('v', LisaaVihollinen); 
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateLeftBorder();
        Level.CreateTopBorder();
        Level.CreateBottomBorder();
        LisaaNappaimet();
        Camera.FollowOffset = new Vector(Screen.Width / 2.5 - RUUDUN_KOKO, 0.0);
        PhysicsObject Maali =  Level.CreateRightBorder();
        Maali.Tag = "oikea";
        LisaaNappaimet();
        Camera.StayInLevel = true;
        SiirraPelaajaaOikeammalle();
        LuoPistelaskuri();
    }

    
    /// <summary>
    /// Muodostaa kenttään katon
    /// </summary>
    /// <param name="paikka">muodostaa paikkavektorin</param>
    /// <param name="leveys">määrittää tason leveyden</param>
    /// <param name="korkeus">määrittää tason korkeuden</param>
    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.Green;
        taso.Tag = "seina";
        Add(taso);
    }

    
    /// <summary>
    /// muodostaa kenttään tähden
    /// </summary>
    /// <param name="paikka">muodostaa paikkavektorin</param>
    /// <param name="leveys">määrittää tähden leveyden</param>
    /// <param name="korkeus">määrittää tähden korkeuden</param>
    private void LisaaTahti(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tahti = PhysicsObject.CreateStaticObject(leveys, korkeus);
        tahti.IgnoresCollisionResponse = true;
        tahti.Position = paikka;
        tahti.Image = tahtiKuva;
        tahti.Tag = "tahti";
        Add(tahti);
    }

    
    /// <summary>
    /// rakentaa pistelaskurin tähti muuttujalle ja osoittaa kerättyjen tähtien määrän 
    /// </summary>
    private void LuoPistelaskuri()
    {
        
        pisteLaskuri = new IntMeter(keratytSalmiakit);               
        Label pisteNaytto = new Label(); 
        pisteNaytto.X = Screen.Left + 100;
        pisteNaytto.Y = Screen.Top - 100;
        pisteNaytto.TextColor = Color.Black;
        pisteNaytto.Color = Color.LightBlue;
        pisteLaskuri.Value= keratytSalmiakit;
        pisteNaytto.BindTo(pisteLaskuri);
        Add(pisteNaytto);
    }

    
    /// <summary>
    /// kun pelaaja törmää esteeseen tai vihuun funktio herää ja lopettaa pelin
    /// </summary>
    private void TormaaKuolemaan(PhysicsObject tormaaja, PhysicsObject kohde)
    {
        if (peliKaynnissa)
        {
            MessageDisplay.Add("Oivoi, et päässyt saunomaan :(");
            Keyboard.Disable(Key.Up);
            peliKaynnissa = false;
            liikutusajastin.Stop();
        }
    }
   
    
    /// <summary>
    /// lisää pelaajan
    /// </summary>
    /// <param name="paikka">muodostaa paikkavektorin</param>
    /// <param name="leveys">määrittää pelaajan leveyden</param>
    /// <param name="korkeus">määrittää pelaajan korkeuden</param>
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja1 = new PlatformCharacter(leveys, korkeus);
        pelaaja1.Position = paikka;
        pelaaja1.Mass = 3.0;
        pelaaja1.Image = pelaajanKuva;
        AddCollisionHandler(pelaaja1, "oikea", TormaaOikeaan);
        AddCollisionHandler(pelaaja1, "tahti", TormaaTahteen);
        AddCollisionHandler(pelaaja1, "katto", TormaaKuolemaan);// CollisionHandler komento kutsuu muuta funktiota kun ehto pelaaja koskettaa tiettyä tägiä
        AddCollisionHandler(pelaaja1, "vihu", TormaaKuolemaan);
        Add(pelaaja1);
    }
   
    
    /// <summary>
    /// lisää vihollisen sekä laittaa sen liikkumaan ylös ja alas
    /// </summary>
    /// <param name="paikka">muodostaa paikkavektorin</param>
    /// <param name="leveys">määrittää vihollisen leveyden</param>
    /// <param name="korkeus">määrittää vihollisen korkeuden</param>
    private void LisaaVihollinen(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject vihollinen = new PhysicsObject(leveys, korkeus);
        vihollinen.Position = paikka;
        Add(vihollinen);
        vihollinen.Image = vihollinenkuva;
        vihollinen.IgnoresGravity = true;
        vihollinen.CanRotate = false;
        vihollinen.IgnoresCollisionResponse = true;
        vihollinen.Oscillate(new Vector(0, 1), korkeus * 1.5, 0.3);
        vihollinen.Tag = "vihu";
    }

    
    /// <summary>
    /// Lisää näppäimet
    /// </summary>
    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja1, HYPPYNOPEUS);
        ControllerOne.Listen(Button.Back, ButtonState.Pressed, Exit, "Poistu pelistä");
        ControllerOne.Listen(Button.A, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja1, HYPPYNOPEUS);
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }

    
    /// <summary>
    /// Liikuttaa hahmoa
    /// </summary>
    /// <param name="hahmo">kutsuu hahmon</param>
    /// <param name="nopeus">laittaa pelaajan kulkemaan sen hetkisellä nopeudella</param>
    private void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
    }

    
    /// <summary>
    /// pakottaa hypyn hypyn perään
    /// </summary>
    /// <param name="hahmo">kutsuu pelaajan </param>
    /// <param name="nopeus">ottaa tämänhetkisen nopeuden ja suhteuttaa hypyn korkeuden nopeuteen sekä pakottaa toisen hypyn putkeen</param>
    private void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.ForceJump(nopeus);
    }

    
    /// <summary>
    /// laittaa törmäyksen tähteen lisäämään arvoa pistelaskurille
    /// </summary>
    /// <param name="hahmo">kutsuu hahmon</param>
    /// <param name="tahti">kutsuu tähden ja määrittää mitä käy kun hahmo ja tähti kohtaavat</param>
    private void TormaaTahteen(PhysicsObject hahmo, PhysicsObject tahti)
    {
        maaliAani.Play();
        MessageDisplay.Add("Sait Salmiakkia! tämä maistuu hyvältä saunassa!");
        tahti.Destroy();
        keratytSalmiakit++;
        pisteLaskuri.Value = keratytSalmiakit;
    }

    
    /// <summary>
    /// tyhjentää edellisen kentän sekä kutsuu uutta kenttää
    /// </summary>
    /// <param name="kenttaNro">katsoo mikä kenttänumero on ja mikä kenttä tehdään seuraavaksi</param>
    private void VaihdaKenttään(int kenttaNro)
    {
        ClearAll();
        Camera.Reset();
        LuoSeuraavaKentta();
        peliKaynnissa = true;
        liikutusajastin = new Timer();
        liikutusajastin.Interval = 0.01;
        liikutusajastin.Timeout += SiirraPelaajaaOikeammalle;
        liikutusajastin.Start();
        peliKaynnissa = true;
    }
    

    /// <summary>
    /// lisää katon sekä esteet
    /// </summary>
    /// <param name="paikka">muodostaa paikkavektorin</param>
    /// <param name="leveys">määrittää esteen leveyden</param>
    /// <param name="korkeus">määrittää esteen korkeuden</param>
    private void LisaaEste(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject Esteet = PhysicsObject.CreateStaticObject(leveys, korkeus);
        Esteet.Position = paikka;
        Esteet.Color = Color.Green; 
        Esteet.Tag = "katto";
        Add(Esteet);
    }
    
    
    /// <summary>
    /// Kun pelaaja törmää oikeaan reunaan eli pääsee kentän läpi, asetetaan kenttanumeroa isommaksi sekä tehdään kenttä 2
    /// </summary>
    /// <param name="hahmo">kutsuu hahmoa ja kertoo mitä sille käy</param>
    /// <param name="maali">kertoo törmäyksen kohteen ja funktio kertoo mitä sitten käy</param>
    private void TormaaOikeaan(PhysicsObject hahmo, PhysicsObject maali)
    {
        maaliAani.Play();
        kentta1lapi = true;
       
        if(kentta1lapi)
        {
            MessageDisplay.Add("Löysit saunajuomat! Löydätkö vielä tien saunaan?");
        }
        else if(kenttaNro>2)
        {
            MessageDisplay.Add("Tervetuloa saunaan, keräsit {$keratytSalmiakit} salmiakkia");
            StopAll();
        }
        LuoSeuraavaKentta();
        peliKaynnissa = true;
    }

    
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}
 //class Program
 //       {
           // static void Main(string[] args)
           // {
           //     for (int i = 1; i <= 100; i++)
           //     {
           //
           //         if (i % 5 == 0 && i != 0) // tarkistaa jaollisuuden
           //         {
           //             Console.WriteLine(i+" Hep!"); // huutaa hep jos jaollinen
           //         }
           //         else 
           //             Console.WriteLine(i);
           //     }
           // }
            
         //   public class nautettavat 
         //   {
         //       public static void Main()
         //       {
         //           int[] luvut = { 12, 3, 5, 9, 7, 1, 4, 9 };
         //           TulostaYli(luvut, 4);
         //       }
         //
         //       public static void TulostaYli(int[] taulukko, int raja)
         //       {
         //           bool ensimmainen = true; //apumuuttuja tuloksien hallintaan
         //           foreach (int luku in taulukko)
         //           {
         //               if (luku > raja)
         //               {
         //                   if (!ensimmainen)
         //                   {
         //                       Console.Write(" "); 
         //                   }
         //                   Console.Write(luku);
         //                   ensimmainen = false; // Ensimmäinen luku on jo tulostettu joten merkitään seuraava
         //               }
         //           }
         //           Console.WriteLine(); // Lopun rivinvaihto
         //       }
         //   }
      //   static string PisinJono(List<string> jonot)
      //   {
      //       if (jonot == null || jonot.Count == 0)
      //       {
      //           return null; // Palautetaan null, jos lista on tyhjä tai null
      //       }
      //
      //       string pisin = jonot[0]; // Alustetaan pisimmäksi merkkijonoksi ensimmäinen alkuun
      //
      //       foreach (string merkkit in jonot)
      //       {
      //           if (merkkit.Length > pisin.Length)
      //           {
      //               pisin = merkkit; // Päivitetään tarvittaessa merkkijonoa
      //           }
      //       }
      //
      //       return pisin;
      //   }
      //   
      //  }
    
