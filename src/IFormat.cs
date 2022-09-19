using static ITags;

class IFormat
{
    public string filename;
    public int nb_streams;
    public int nb_programs;
    public string format_name;
    public string format_long_name;
    public string start_time;
    public string duration;
    public string size;
    public string bit_rate;
    public int probe_score;
    public ITags tags;
}
