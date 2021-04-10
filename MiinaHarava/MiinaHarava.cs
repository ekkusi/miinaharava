using System;
using System.Collections.Generic;
using System.Windows;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

/// @author Ekku Sipilä
/// @version 1.0


/// <summary>
/// Luokka Solut, jota käytetään jokaisen ruudun ja sen ominaisuuksien kuvaamiseen
/// </summary>
public class Solut
{
    // Luokan attribuutit onNakyvissa, onMiina, onMerkitty sekä onKlikattuMiina, joita voidaan hakea ja muuttaa, ja
    // jotka määrittelevät onko ruutu näkyvissä, onko se miina, onko siinä lippua ja onko se miina, jota klikattiin
    public bool onNakyvissa, onMiina, onMerkitty, onKlikattuMiina;
    // Määrittelevät montako miinaa on solun ympärillä
    public int Naapurit { get; set; }
    // Naapurit-muuttujan arvo label tekstinä
    public Label NaapuriTeksti { get; set; }
    // GameObject, joka jokaisesta solusta lisätään peliin
    public GameObject Ruutu  { get; set; }
    /// <summary>
    /// Luokan olion määrittely
    /// </summary>
    /// <param name="onNakyvissa">Onko ruutu näkyvissä</param>
    /// <param name="onMiina">Onko ruutu miina</param>
    /// <param name="onMerkitty">Onko ruutu merkitty lipulla</param>
    /// <param name="onKlikattuMiina">Onko ruutu miina, jota klikattiin</param>
    public Solut(bool onNakyvissa, bool onMiina, bool onMerkitty, bool onKlikattuMiina)
    {
        this.onNakyvissa = onNakyvissa;
        this.onMiina = onMiina;
        this.onMerkitty = onMerkitty;
        this.onKlikattuMiina = onKlikattuMiina;
    }
}


/// <summary>
/// Luokka Miinaharava, jossa kaikki pelin toiminnot
/// </summary>
public class MiinaHarava : Game
{
    int sarakkeet;
    int rivit;
    int miinaLkm;
    int miinattomat;
    int lopetusLaskuri = 0;
    int vaikeusValinta;
    IntMeter miinaLaskuri;
    Timer aikaLaskuri;
    Timer ajastin;
    Image alkuBoxi = LoadImage("box");
    GameObject hymio;
    Solut[,] solut;
    /// <summary>
    /// Begin aloittaa ohjelman. Kentään koon kysyminen pelaajalta
    /// </summary>
    public override void Begin()
    {
        MultiSelectWindow valikko = new MultiSelectWindow("Valitse kentän koko", "Pieni", "Keskikokoinen", "Suuri")
        {
            SelectionColor = Color.LightGray
        };
        valikko.SetButtonColor(Color.LightGray);
        valikko.SetButtonTextColor(Color.Black);
        valikko.ItemSelected += KoonAsetus;
        Add(valikko);

 

        Level.BackgroundColor = Color.Black;

        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Rivien ja sarakkeiden määrän määritteleminen pelaajan valinnan mukaan
    /// </summary>
    /// <param name="valinta">Pelaajan valinnan indeksi</param>
    public void KoonAsetus(int valinta)
    {
        switch (valinta) {
            case 0:
                rivit = sarakkeet = 10;
                break;
            case 1:
                rivit = sarakkeet = 20;
                break;
            case 2:
                rivit = sarakkeet = 30;
                break;
        }

        VaikeusAste();
    }


    /// <summary>
    /// Luo ikkunan vaikeuden valinnalle
    /// </summary>
    public void VaikeusAste()
    {
        MultiSelectWindow valikko2 = new MultiSelectWindow("Valitse vaikeus", "1", "2", "3")
        {
            SelectionColor = Color.LightGray
        };
        valikko2.SetButtonColor(Color.LightGray);
        valikko2.SetButtonTextColor(Color.Black);
        valikko2.ItemSelected += VaikeudenAsetus;
        vaikeusValinta = valikko2.SelectedIndex;
        Add(valikko2);
    }
    /// <summary>
    /// Asettaa miinojen määrän vaikeuden valinnan ja koon mukaan
    /// </summary>
    /// <param name="valinta">Pelaajan valinnan indeksi</param>
    public void VaikeudenAsetus(int valinta)
    {
        switch (valinta) {
            case 0:
                miinaLkm = rivit * sarakkeet / 8;
                break;
            case 1:
                miinaLkm = rivit * sarakkeet / 6;
                break;
            case 2:
                miinaLkm = rivit * sarakkeet / 4;
                break;
        }
        miinattomat = rivit * sarakkeet - miinaLkm;

        Aloitus();
    }


    /// <summary>
    /// Luo pelin kenttään kuuluvat elementit
    /// </summary>
    public void Aloitus()
    {
        const int soluKoko = 25;

        SetWindowSize(sarakkeet * soluKoko, rivit * soluKoko + 50);
        Level.Size = new Vector(sarakkeet * soluKoko, rivit * soluKoko + 50);

        GameObject ylaPalkki = new GameObject(Level.Width, 50, Shape.Rectangle)
        {
            X = 0,
            Y = Level.Top - 50 / 2,
            Color = Color.LightGray
        };
        Add(ylaPalkki);

        MustaLaatikko(60, 40, Level.Left + 35, ylaPalkki.Y);
        MustaLaatikko(60, 40, Level.Right - 35, ylaPalkki.Y);

        miinaLaskuri = new IntMeter(miinaLkm);
        Label miinaLabel = LaskuriLabel(Level.Left + 35, ylaPalkki.Y);
        miinaLabel.BindTo(miinaLaskuri);
        Add(miinaLabel);

        aikaLaskuri = new Timer();
        aikaLaskuri.Start(999);
        Label aikaLabel = LaskuriLabel(Level.Right - 35, ylaPalkki.Y);
        aikaLabel.BindTo(aikaLaskuri.SecondCounter);
        Add(aikaLabel);

        Image hymy = LoadImage("hymy");
        hymio = new GameObject(40, 40, Shape.Rectangle)
        {
            X = 0,
            Y = Level.Top - 25,
            Image = hymy
        };
        Add(hymio);

        Image[] hymyKuvat = new Image[2];
        Image hammastys = LoadImage("hammastys");
        hymyKuvat[0] = hymy;
        hymyKuvat[1] = hammastys;

        LuoSoluTaulukko(soluKoko);
        MiinojenAsetus();
        LaskeNaapuriMiinat();

        Keyboard.Listen(Key.X, ButtonState.Pressed, KlikattuRuutu, null, hymyKuvat);
        Keyboard.Listen(Key.Z, ButtonState.Pressed, MerkitseMiina, null);
    }


    /// <summary>
    /// Luo labelin annetuilla parametreilla
    /// </summary>
    /// <param name="x">Labelin X-paikka</param>
    /// <param name="y">Labelin Y-paikka</param>
    /// <returns>Luotu label</returns>
    public Label LaskuriLabel(double x, double y)
    {
        Label label = new Label()
        {
            X = x,
            Y = y,
            Color = Color.Black,
            TextColor = Color.Red,
            Font = Font.DefaultLarge,
            DecimalPlaces = 0
        };
        return label;
    }


    /// <summary>
    /// Luo labelin takana olevan mustan boxin
    /// </summary>
    /// <param name="width">Leveys</param>
    /// <param name="height">Korkeus</param>
    /// <param name="x">X-paikka</param>
    /// <param name="y">Y-paikka</param>
    public void MustaLaatikko(double width, double height, double x, double y)
    {
        GameObject laatikko = new GameObject(width, height, Shape.Rectangle)
        {
            X = x,
            Y = y,
            Color = Color.Black
        };
        Add(laatikko);
    }

    
    /// <summary>
    /// Luo jokaiselle solut taulukon paikalle oman Solut-objektin ja lisää niiden Ruudut kentälle
    /// </summary>
    public void LuoSoluTaulukko(int soluKoko)
    {
        solut = new Solut[sarakkeet, rivit];
        for (int i = 0; i < sarakkeet; i++)
        {
            for (int j = 0; j < rivit; j++)
            {
                solut[i, j] = new Solut(false, false, false, false)
                {
                    Ruutu = new GameObject(soluKoko, soluKoko, Shape.Rectangle)
                    {
                        X = Level.Left + (i * soluKoko) + soluKoko / 2,
                        Y = Level.Top - (j * soluKoko) - soluKoko / 2 - 50,
                        Image = alkuBoxi
                    },
                };

                Add(solut[i, j].Ruutu);
            }
        }
    }


    /// <summary>
    /// Asettaa miinaLkm verran miinoja satunnaisiin soluihin
    /// </summary>
    public void MiinojenAsetus()
    {
        Random rnd = new Random();
        for (int i = 0; i < miinaLkm; i++)
        {
            int rivi = rnd.Next(rivit);
            int sarake = rnd.Next(sarakkeet);
            // Arpoo uudelleen siihe asti, että solussa ei ole  valimiksi miinaa
            while (solut[sarake, rivi].onMiina == true)
            {
                rivi = rnd.Next(rivit);
                sarake = rnd.Next(sarakkeet);
            }
            solut[sarake, rivi].onMiina = true;
        }
    }


    /// <summary>
    /// Laskee soluille montako sen naapureistasoluista on miinoja
    /// </summary>
    public void LaskeNaapuriMiinat()
    {
        int kaikkiNaapurit;
        for (int i = 0; i < sarakkeet; i++)
        {
            for (int j = 0; j < rivit; j++)
            {
                kaikkiNaapurit = 0;
                // solua ympäröivät solut
                for (int y = -1; y < 2; y++)
                {
                    for (int x = -1; x < 2; x++)
                    {
                        if ((i + y) > -1 && (i + y) < sarakkeet && (j + x) > -1 && (j + x) < rivit)
                        {
                            if (solut[i + y, j + x].onMiina == true)
                            {
                                kaikkiNaapurit++;
                            }
                        }
                    }
                }
                solut[i, j].Naapurit = kaikkiNaapurit;
            }
        }
    }


    /// <summary>
    /// Etsii ruudun hiiren paikan mukaan ja kutsuu PaljastaRuutu aliohjelmaa sen mukaan.
    /// Jos klikkaus on ensimmäinen pelin klikkaus ja ruutu on miina, luodaan kenttä uudelleen
    /// niin pitkää kunnes paikalla ei ole enää miinaa
    /// </summary>
    /// <param name="kuvat">Hymyn ja hämmästyksen kuvat</param>
    public void KlikattuRuutu(Image[] kuvat)
    {
        double x = Mouse.PositionOnScreen.X;
        double y = Mouse.PositionOnScreen.Y;
        for (int i = 0; i < sarakkeet; i++)
        {
            for (int j = 0; j < rivit; j++)
            {
                // onko klikkaus solun ruudun sisällä
                if (x > (solut[i, j].Ruutu.X - solut[i, j].Ruutu.Width / 2) && x < (solut[i, j].Ruutu.X - solut[i, j].Ruutu.Width / 2 + solut[i, j].Ruutu.Width) &&
                    y > (solut[i, j].Ruutu.Y - solut[i, j].Ruutu.Height / 2) && y < (solut[i, j].Ruutu.Y - solut[i, j].Ruutu.Height / 2 + solut[i, j].Ruutu.Height))
                {
                    if (solut[i, j].onMerkitty == false && solut[i, j].onNakyvissa == false)
                    {
                        solut[i, j].onNakyvissa = true;
                        // jos ensimmäinen klikkaus on miina, aloitetaan peli uudelleen (ensimmäinen klikkaus ei voi olla miina)
                        if (solut[i, j].onMiina == true && lopetusLaskuri == 0)
                        {
                            ClearAll();

                            lopetusLaskuri = 0;
                            VaikeudenAsetus(vaikeusValinta);

                            Level.BackgroundColor = Color.Black;
                            Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
                            
                            KlikattuRuutu(kuvat);
                        }
                        else if (solut[i, j].onMiina == true)
                        {
                            solut[i, j].onKlikattuMiina = true;
                            PaljastaRuutu(i, j);
                            PaljastaKaikkiRuudut();
                            LopetusHavio();
                        }
                        else
                        {
                            KuvanVaihto(kuvat);
                            if (hymio.Image.Name == kuvat[1].Name)
                            {
                                ajastin = new Timer();
                                ajastin.Interval += 0;
                                ajastin.Timeout += delegate { KuvanVaihto(kuvat); } ;
                                ajastin.Start();
                            }

                            if (solut[i, j].Naapurit == 0)
                            {
                                PaljastaYmparys(i, j);
                            }
                            PaljastaRuutu(i, j);
                        }
                    }
                    return;
                }
            }
        }
    }


    /// <summary>
    /// Vaihtaa hymion kuvan
    /// </summary>
    /// <param name="kuvat">Hymyn ja hämmästyksen kuvat</param>
    public void KuvanVaihto(Image[] kuvat)
    {
        if (hymio.Image.Name == kuvat[0].Name)
        {
            hymio.Image = kuvat[1];
        }
        else if(hymio.Image.Name == kuvat[1].Name)
        {
            ajastin.Stop();
            hymio.Image = kuvat[0];
        }
    }


    /// <summary>
    /// Vaihtaa ruudun kuvan sen ominaisuuksien mukaan ja vaihtaa tarvittavat ominaisuudet
    /// </summary>
    /// <param name="i">Sarakekohta</param>
    /// <param name="j">Rivikohta</param>
    public void PaljastaRuutu(int i, int j)
    {
        if (solut[i, j].onMiina == true)
        {
            if (solut[i, j].onKlikattuMiina == true)
            {
                Image klikattuMiina = LoadImage("klikattumiina");
                solut[i, j].Ruutu.Image = klikattuMiina;
            }
            else
            {
                Image miina = LoadImage("miina");
                solut[i, j].Ruutu.Image = miina;
            }
        }
        else
        {
            Image klikattuBoxi = LoadImage("klikattuboxi");
            lopetusLaskuri++;
            if (solut[i, j].Naapurit > 0)
            {
                solut[i, j].Ruutu.Image = klikattuBoxi;
                solut[i, j].NaapuriTeksti = new Label()
                {
                    Text = solut[i, j].Naapurit.ToString(),
                    X = solut[i, j].Ruutu.X,
                    Y = solut[i, j].Ruutu.Y
                };
                if(solut[i, j].Naapurit == 1)
                {
                    solut[i, j].NaapuriTeksti.TextColor = Color.Blue;
                }
                if (solut[i, j].Naapurit == 2)
                {
                    solut[i, j].NaapuriTeksti.TextColor = Color.Green;
                }
                if (solut[i, j].Naapurit == 3)
                {
                    solut[i, j].NaapuriTeksti.TextColor = Color.Red;
                }
                if (solut[i, j].Naapurit == 4)
                {
                    solut[i, j].NaapuriTeksti.TextColor = Color.DarkBlue;
                }
                if (solut[i, j].Naapurit == 5)
                {
                    solut[i, j].NaapuriTeksti.TextColor = Color.BloodRed;
                }
                if (solut[i, j].Naapurit == 6)
                {
                    solut[i, j].NaapuriTeksti.TextColor = Color.LightBlue;
                }
                if (solut[i, j].Naapurit == 7)
                {
                    solut[i, j].NaapuriTeksti.TextColor = Color.Black;
                }
                if (solut[i, j].Naapurit == 8)
                {
                    solut[i, j].NaapuriTeksti.TextColor = Color.Gray;
                }
                Add(solut[i, j].NaapuriTeksti);
            }
            else
            {
                solut[i, j].Ruutu.Image = klikattuBoxi;
            }
            if(lopetusLaskuri == miinattomat)
            {
                PaljastaKaikkiRuudut();
                LopetusVoitto();
            }
        }
    }


    /// <summary>
    /// Paljastaa ruudun naapuriruudut
    /// </summary>
    /// <param name="i">Sarakekohta</param>
    /// <param name="j">Rivikohta</param>
    public void PaljastaYmparys(int i, int j)
    {
        for (int y = -1; y < 2; y++)
        {
            for (int x = -1; x < 2; x++)
            {
                if ((i + y) > -1 && (i + y) < sarakkeet && (j + x) > -1 && (j + x) < rivit)
                {
                    if (solut[i + y, j + x].onNakyvissa == false)
                    {
                        if (solut[i + y, j + x].onMerkitty == true && solut[i + y, j + x].onMiina == false) miinaLaskuri.Value++;
                        solut[i + y, j + x].onNakyvissa = true;
                        PaljastaRuutu(i + y, j + x);
                        if (solut[i + y, j + x].Naapurit == 0)
                        {
                            PaljastaYmparys(i + y, j + x);
                        }
                    }
                }
            }
        }
    }


    /// <summary>
    /// Paljastaa kaikki ruudut
    /// </summary>
    public void PaljastaKaikkiRuudut()
    {
        for (int i = 0; i < sarakkeet; i++)
        {
            for (int j = 0; j < rivit; j++)
            {
                if (solut[i, j].onMerkitty == false && solut[i ,j].onKlikattuMiina == false && solut[i, j].onNakyvissa == false)
                {
                    solut[i, j].onNakyvissa = true;
                    PaljastaRuutu(i, j);
                }
                else if(solut[i, j].onMerkitty == true && solut[i, j].onMiina == false)
                {
                    Image vaaraLippu = LoadImage("vaaralippu");
                    solut[i, j].Ruutu.Image = vaaraLippu;
                }
            }
        }
    }


    /// <summary>
    /// Merkitsee tai poistaa ruudun vaihtamalla/poistamalla ruudun kuvan lipuksi
    /// </summary>
    public void MerkitseMiina()
    {
        double x = Mouse.PositionOnScreen.X;
        double y = Mouse.PositionOnScreen.Y;
        for (int i = 0; i < sarakkeet; i++)
        {
            for (int j = 0; j < rivit; j++)
            {
                if (x > (solut[i, j].Ruutu.X - solut[i, j].Ruutu.Width / 2) && x < (solut[i, j].Ruutu.X - solut[i, j].Ruutu.Width / 2 + solut[i, j].Ruutu.Width) &&
                    y > (solut[i, j].Ruutu.Y - solut[i, j].Ruutu.Height / 2) && y < (solut[i, j].Ruutu.Y - solut[i, j].Ruutu.Height / 2 + solut[i, j].Ruutu.Height))
                {
                    if (solut[i, j].onNakyvissa == false)
                    {
                        if (solut[i, j].onMerkitty == false)
                        {
                            Image lippu = LoadImage("flag");
                            miinaLaskuri.Value--;
                            solut[i, j].onMerkitty = true;
                            solut[i, j].Ruutu.Image = lippu;
                        }
                        else
                        {
                            miinaLaskuri.Value++;
                            solut[i, j].onMerkitty = false;
                            solut[i, j].Ruutu.Image = alkuBoxi;
                        }
                    }
                    return;
                }
            }
        }
    }


    /// <summary>
    /// Lopettaa aikalaskurin, vaihtaa hymiön kuvan voiton kuvaksi ja luo valikon pelaajalle
    /// mistä mahdollisuus lopettaa, pelata uudelleen tai mennä valikkoon
    /// </summary>
    public void LopetusVoitto()
    {
        aikaLaskuri.Stop();
        Image voitto = LoadImage("voitto");
        hymio.Image = voitto;

        MultiSelectWindow valikko3 = new MultiSelectWindow("Voitit pelin!", "Lopeta", "Pelaa uudelleen", "Valikkoon")
        {
            SelectionColor = Color.LightGray
        };
        valikko3.SetButtonColor(Color.LightGray);
        valikko3.SetButtonTextColor(Color.Black);
        valikko3.ItemSelected += LopetusProsessointi;
        Add(valikko3);
    }


    /// <summary>
    /// Lopettaa aikalaskurin, vaihtaa hymiön kuvan häviön kuvaksi ja luo valikon pelaajalle
    /// mistä mahdollisuus lopettaa, pelata uudelleen tai mennä valikkoon
    /// </summary>
    public void LopetusHavio()
    {
        Image havio = LoadImage("havio");
        hymio.Image = havio;
        aikaLaskuri.Stop();

        MultiSelectWindow valikko4 = new MultiSelectWindow("Hävisit pelin", "Lopeta", "Pelaa uudelleen", "Valikkoon")
        {
            SelectionColor = Color.LightGray
        };
        valikko4.SetButtonColor(Color.LightGray);
        valikko4.SetButtonTextColor(Color.Black);
        valikko4.ItemSelected += LopetusProsessointi;
        Add(valikko4);
    }


    /// <summary>
    /// Pelin lopetus, uudelleen pelaaminen samoilla asetuksilla tai valikkoon meneminen sen mukaan
    /// mitä pelaaja valitsi
    /// </summary>
    /// <param name="valinta">Valinnan indeksi</param>
    public void LopetusProsessointi(int valinta)
    {
        switch (valinta)
        {
            case 0:
                Exit();
                break;
            case 1:
                ClearAll();
                lopetusLaskuri = 0;
                VaikeudenAsetus(vaikeusValinta);
                Level.BackgroundColor = Color.Black;
                Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
                break;
            case 2:
                ClearAll();
                lopetusLaskuri = 0;
                Begin();
                break;
        }
    }
}
