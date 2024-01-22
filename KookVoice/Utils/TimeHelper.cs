namespace RankedUtils.Utils {
    public class TimeHelper {
        public static bool TryParseTime(string time, DateTimeOffset start, out DateTimeOffset result) {
            int stack = 0;
            result = start;
            foreach (char t in time) {
                switch (t) {
                    case 'y':
                    {
                        result = result.AddYears(stack);
                        break;
                    }
                    case 'M':
                    {
                        result = result.AddMinutes(stack);
                        break;
                    }
                    case 'd':
                    {
                        result = result.AddDays(stack);
                        break;
                    }
                    case 'h':
                    {
                        result = result.AddHours(stack);
                        break;
                    }
                    case 'm':
                    {
                        result = result.AddMinutes(stack);
                        break;
                    }
                    case 's':
                    {
                        result = result.AddSeconds(stack);
                        break;
                    }
                    default:
                    {
                        if (int.TryParse(new string(t, 1), out int x)) {
                            stack *= 10;
                            stack += x;
                        } else
                            return false;
                        break;
                    }
                }
            }
            return true;
        }
    }
}
