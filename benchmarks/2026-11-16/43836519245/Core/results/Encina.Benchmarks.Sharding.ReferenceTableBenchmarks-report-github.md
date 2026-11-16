```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|--------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    51,031.768 ns |    122.4407 ns |   114.5311 ns |  1.000 |    0.00 |    6 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   554,874.354 ns |  1,606.2198 ns | 1,502.4590 ns | 10.873 |    0.04 |    7 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 3,024,777.393 ns |  5,864.2085 ns | 5,198.4681 ns | 59.273 |    0.16 |    8 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |        10.669 ns |      0.0252 ns |     0.0223 ns |  0.000 |    0.00 |    3 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         5.375 ns |      0.0147 ns |     0.0137 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         7.492 ns |      0.0132 ns |     0.0117 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       103.891 ns |      0.1838 ns |     0.1719 ns |  0.002 |    0.00 |    5 | 0.0013 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        13.048 ns |      0.0203 ns |     0.0190 ns |  0.000 |    0.00 |    4 |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |               |        |         |      |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    51,045.922 ns |    734.9933 ns |    40.2875 ns |  1.000 |    0.00 |    5 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   550,405.087 ns |  8,536.3849 ns |   467.9081 ns | 10.783 |    0.01 |    6 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 3,032,526.034 ns | 97,225.7570 ns | 5,329.2718 ns | 59.408 |    0.10 |    7 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         9.109 ns |     12.1336 ns |     0.6651 ns |  0.000 |    0.00 |    2 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         5.352 ns |      0.0729 ns |     0.0040 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         7.501 ns |      0.1168 ns |     0.0064 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       105.606 ns |      2.6025 ns |     0.1427 ns |  0.002 |    0.00 |    4 | 0.0013 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        13.136 ns |      0.0851 ns |     0.0047 ns |  0.000 |    0.00 |    3 |      - |         - |       0.000 |
