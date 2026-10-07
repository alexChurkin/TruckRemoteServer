using System;
using System.Collections.Generic;

namespace TruckRemoteServer.Telemetry
{
    //What the telemetry doesn't tell about a gearbox. The games show the names of the gears, the telemetry gives their
    //numbers: a gearbox with crawler gears counts its gears after them (gear 5 of a 12+2 gearbox is "3"), and
    //a few gearboxes name their top gear "OD". The definitions of the games tell that (crawls and transmission_names
    //of accessory_transmission_data), the telemetry doesn't, so the gearboxes that have it are listed here and
    //recognised by their forward ratios. A gearbox that isn't here (a new truck, a mod) is shown by its numbers.
    public sealed class Gearbox
    {
        //Ratios of the definitions have 2-3 decimals
        private const float RatioTolerance = 0.006f;

        //From def/vehicle/truck/*/transmission of Euro Truck Simulator 2 and American Truck Simulator 1.5x:
        //all the gearboxes with crawler gears or named gears among about 380
        private static readonly Gearbox[] Known =
        {
            //Scania GRSO 925 (R, Streamline, R 2016): C1, C2, 1-12
            new Gearbox(TruckTelemetry.GameUnknown, 2, false,
                13.26f, 10.625f, 9.15f, 7.331f, 5.81f, 4.659f, 3.75f, 3.01f, 2.444f, 1.955f, 1.553f, 1.242f, 1f, 0.8f),
            //Scania G25CM, G33CM, G38CM (R and S 2016): C, 1-12, OD. International LT of American Truck Simulator
            //has a gearbox with the same ratios and plain numbers, so the game is told
            new Gearbox(TruckTelemetry.GameEts2, 1, true,
                20.81f, 16.16f, 12.57f, 9.76f, 7.56f, 5.87f, 4.55f, 3.53f, 2.77f, 2.15f, 1.66f, 1.29f, 1f, 0.78f),
            //Volvo ATO 3512F (FH16), ATO 2612G (VNL 2025): C1, C2, 1-12
            new Gearbox(TruckTelemetry.GameUnknown, 2, false,
                32.04f, 19.38f, 11.73f, 9.21f, 7.09f, 5.57f, 4.35f, 3.41f, 2.7f, 2.12f, 1.63f, 1.28f, 1f, 0.78f),
            //Renault ATO 2614F (T): C1, C2, 1-12
            new Gearbox(TruckTelemetry.GameUnknown, 2, false,
                32.04f, 19.38f, 11.73f, 9.21f, 7.09f, 5.57f, 4.35f, 3.41f, 2.7f, 2.12f, 1.63f, 1.28f, 1f, 0.79f),
            //Mack mDRIVE HD 14 (Pinnacle): C1, C2, 1-12
            new Gearbox(TruckTelemetry.GameUnknown, 2, false,
                32.04f, 19.98f, 11.73f, 9.21f, 7.09f, 5.57f, 4.35f, 3.41f, 2.7f, 2.12f, 1.63f, 1.28f, 1f, 0.78f),
            //Volvo AT 2612G (VNL 2025): C, 1-12
            new Gearbox(TruckTelemetry.GameUnknown, 1, false,
                19.38f, 14.94f, 11.73f, 9.04f, 7.09f, 5.54f, 4.35f, 3.44f, 2.7f, 2.08f, 1.63f, 1.27f, 1f)
        };

        private readonly int game;
        private readonly float[] forwardRatios;

        private Gearbox(int game, int crawlerGears, bool topGearIsOverdrive, params float[] forwardRatios)
        {
            this.game = game;
            CrawlerGears = crawlerGears;
            TopGearIsOverdrive = topGearIsOverdrive;
            this.forwardRatios = forwardRatios;
        }

        //How many of the first forward gears are crawler gears, named apart (C1, C2) and not counted by the game
        public int CrawlerGears { get; }

        //The top gear is named "OD" by the game
        public bool TopGearIsOverdrive { get; }

        public int ForwardGears => forwardRatios.Length;

        //The gearbox with these forward ratios in the game, null if it isn't one of the listed ones
        public static Gearbox Find(int game, IReadOnlyList<float> ratios)
        {
            if (ratios == null) return null;
            foreach (Gearbox gearbox in Known)
            {
                if (gearbox.game != TruckTelemetry.GameUnknown && gearbox.game != game) continue;
                if (gearbox.Has(ratios)) return gearbox;
            }
            return null;
        }

        private bool Has(IReadOnlyList<float> ratios)
        {
            if (ratios.Count != forwardRatios.Length) return false;
            for (int i = 0; i < forwardRatios.Length; i++)
            {
                if (Math.Abs(ratios[i] - forwardRatios[i]) > RatioTolerance) return false;
            }
            return true;
        }

        //What the game shows for a gear of the telemetry (positive - forward): the number counted after the crawler
        //gears, or the number of the crawler gear itself. An unknown gearbox (null) shows its gears as they are
        public static int ShownGear(Gearbox gearbox, int gear)
        {
            if (gearbox == null || gear <= gearbox.CrawlerGears) return gear;
            return gear - gearbox.CrawlerGears;
        }

        public static bool IsCrawlerGear(Gearbox gearbox, int gear)
        {
            return gearbox != null && gear > 0 && gear <= gearbox.CrawlerGears;
        }

        public static bool IsOverdrive(Gearbox gearbox, int gear)
        {
            return gearbox != null && gearbox.TopGearIsOverdrive && gear == gearbox.ForwardGears;
        }
    }
}
