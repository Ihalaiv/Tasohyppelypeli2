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
namespace Pororun;
/// @author Kaljami
/// @version 03.03.2024
/// <summary>
/// 
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class Pororun : PhysicsGame
{
    private const int RuudunKoko = 40;
    private const double Nopeus = 7500;
    private double NykyinenNopeus = Nopeus;
    private const double HYPPYNOPEUS = 250;
    private PlatformCharacter pelaaja1;
    private Image pelaajanKuva = LoadImage("hahmo.png");
    private Image tahtiKuva = LoadImage("salmiakkia.png");
    private Timer liikutusajastin;
    private Image  vihollinenkuva= LoadImage("vihu.png");
    private SoundEffect maaliAani = LoadSoundEffect("maali.wav");
    private bool peliKaynnissa = false;
    private bool kentta1lapi = false;
    int _lapaistutkentat=0;
    private int _keratytSalmiakit = 0;/// <summary>
                                      /// _keratytSalmiakit kasvaa kun kerätään piste pelissä
                                      /// </summary>
    int _kenttaNro = 1;
    IntMeter pisteLaskuri;/// <summary>
                          /// muodostaa PisteLaskurin joka merkkaa kerätyt pisteet
                          /// </summary>
    public override void Begin()
    {
        Gravity = new Vector(0, -800);
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
    void PelaajanNopeus()
        {
            NykyinenNopeus *= 1.00005;
        }
        
    void SiirraPelaajaaOikeammalle()
    {
        PelaajanNopeus(); 
        pelaaja1.Push(new Vector(NykyinenNopeus, 0.0));// Pakottaa pelaajan liikkumaan oikealle
    }
    
    private void LuoSeuraavaKentta()
    {
        if(_kenttaNro==1)
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

    void LuoKentta(string taso)
    {
       ClearAll();
       Camera.Reset();
        TileMap kentta = TileMap.FromLevelAsset(taso);
        kentta.SetTileMethod('=', LisaaEste);
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('*', LisaaTahti);
        kentta.SetTileMethod('J', LisaaPelaaja);
        kentta.SetTileMethod('v', LisaaVihollinen); 
        kentta.Execute(RuudunKoko, RuudunKoko);
        Level.CreateLeftBorder();
        Level.CreateTopBorder();
        Level.CreateBottomBorder();
        LisaaNappaimet();
        Camera.FollowOffset = new Vector(Screen.Width / 2.5 - RuudunKoko, 0.0);
        PhysicsObject maali =  Level.CreateRightBorder();
        maali.Tag = "oikea";
        LisaaNappaimet();
        Camera.StayInLevel = true;
        SiirraPelaajaaOikeammalle();
        LuoPistelaskuri();
    }
    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.Green;
        taso.Tag = "seina";
        Add(taso);
    }

    private void LisaaTahti(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tahti = PhysicsObject.CreateStaticObject(leveys, korkeus);
        tahti.IgnoresCollisionResponse = true;
        tahti.Position = paikka;
        tahti.Image = tahtiKuva;
        tahti.Tag = "tahti";
        Add(tahti);///lisää salmiakit
    }

    void LuoPistelaskuri()
    {
        pisteLaskuri = new IntMeter(_keratytSalmiakit);               
        Label pisteNaytto = new Label(); 
        pisteNaytto.X = Screen.Left + 100;
        pisteNaytto.Y = Screen.Top - 100;
        pisteNaytto.TextColor = Color.Black;
        pisteNaytto.Color = Color.LightBlue;
        pisteLaskuri.Value= _keratytSalmiakit;
        pisteNaytto.BindTo(pisteLaskuri);
        Add(pisteNaytto);
    }
    void TormaaKuolemaan(PhysicsObject tormaaja, PhysicsObject kohde)
    {
        if (peliKaynnissa)
        {
            MessageDisplay.Add("Oivoi, et päässyt saunomaan :(");
            Keyboard.Disable(Key.Up);
            peliKaynnissa = false;
            liikutusajastin.Stop();
        }
    }
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja1 = new PlatformCharacter(leveys, korkeus);
        pelaaja1.Position = paikka;
        pelaaja1.Mass = 3.0;
        pelaaja1.Image = pelaajanKuva;
        AddCollisionHandler(pelaaja1, "oikea", TormaaOikeaan);
        AddCollisionHandler(pelaaja1, "tahti", TormaaTahteen);
        AddCollisionHandler(pelaaja1, "katto", TormaaKuolemaan);
        AddCollisionHandler(pelaaja1, "vihu", TormaaKuolemaan);
        Add(pelaaja1);
    }

    void LisaaVihollinen(Vector paikka, double leveys, double korkeus)
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

    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja1, HYPPYNOPEUS);
        Keyboard.Listen(Key.R, ButtonState.Pressed,AloitaAlusta , "Aloita alusta");
        ControllerOne.Listen(Button.Back, ButtonState.Pressed, Exit, "Poistu pelistä");
        ControllerOne.Listen(Button.A, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja1, HYPPYNOPEUS);
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }
 

    private void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.ForceJump(nopeus);
    }

    private void TormaaTahteen(PhysicsObject hahmo, PhysicsObject tahti)
    {
        maaliAani.Play();
        MessageDisplay.Add("Sait Salmiakkia! tämä maistuu hyvältä saunassa!");
        tahti.Destroy();
        _keratytSalmiakit++;
        pisteLaskuri.Value = _keratytSalmiakit;/// pistelaskuri saa arvon lisää kun _keratytSalmiakit kasvaa
    }
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

    private void AloitaAlusta()
    {
        VaihdaKenttään(1);
        _keratytSalmiakit = 0;
    }

    private void LisaaEste(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject Esteet = PhysicsObject.CreateStaticObject(leveys, korkeus);
        Esteet.Position = paikka;
        Esteet.Color = Color.Green; 
        Esteet.Tag = "katto";
        Add(Esteet);
    }
    
    void TormaaOikeaan(PhysicsObject hahmo, PhysicsObject maali)
    {
        maaliAani.Play();
        kentta1lapi = true;
        _lapaistutkentat++;
        _kenttaNro++;
        if(kentta1lapi)
        {
            MessageDisplay.Add("Löysit saunajuomat! Löydätkö vielä tien saunaan?");
        }
        else if(_lapaistutkentat>2)
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
    