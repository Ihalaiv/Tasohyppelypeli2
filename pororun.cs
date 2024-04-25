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
    private const double NOPEUS = 7500;
    private double nykyinenNopeus = NOPEUS;
    private const double HYPPYNOPEUS = 250;
    private PlatformCharacter pelaaja1;
    private Image pelaajanKuva = LoadImage("hahmo.png");
    private Image tahtiKuva = LoadImage("salmiakkia.png");
    private Timer liikutusajastin;
    private Image  vihollinenkuva= LoadImage("vihu.png");
    private SoundEffect maaliAani = LoadSoundEffect("maali.wav");
    private bool peliKaynnissa = false;
    private bool kentta1lapi = false;
    private int lapaistutkentat = 0;
    private int keratytSalmiakit = 0;
    IntMeter pisteLaskuri;

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
            nykyinenNopeus *= 1.00005;
        }
        
    void SiirraPelaajaaOikeammalle()
    {
        PelaajanNopeus(); 
        pelaaja1.Push(new Vector(nykyinenNopeus, 0.0));
    }
    
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
        Add(tahti);
    }

    void LuoPistelaskuri()
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
    private void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
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
        keratytSalmiakit++;
        pisteLaskuri.Value = keratytSalmiakit;
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
        kentta1lapi = false;
        kentta2lapi = false;
        keratytSalmiakit = 0;
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
        Lapaistutkentat++;
        kenttaNro++;
        if(kentta1lapi)
        {
            MessageDisplay.Add("Löysit saunajuomat! Löydätkö vielä tien saunaan?");
        }
        else if(Lapaistutkentat>2)
        {
            MessageDisplay.Add("Tervetuloa saunaan, keräsit {$keratytSalmiakit} salmiakkia");
            StopAll();
        }
        LuoSeuraavaKentta();
        peliKaynnissa = true;
    }
 //   private void AloitaAlusta()
 //   {
 //      
 //           ClearAll();  
 //           kentta1lapi = false; 
 //           kentta2lapi = false;
 //           keratytSalmiakit = 0;  
 //           LuoSeuraavaKentta(); 
 //   }
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}
