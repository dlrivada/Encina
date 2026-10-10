```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|--------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    77,146.073 ns |    153.7403 ns |   120.0303 ns |  1.000 |    0.00 |    5 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   834,744.231 ns |    992.1012 ns |   879.4719 ns | 10.820 |    0.02 |    6 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,401,417.136 ns |  9,893.8369 ns | 8,770.6287 ns | 57.053 |    0.14 |    7 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.530 ns |      0.1182 ns |     0.1106 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.046 ns |      0.0060 ns |     0.0057 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         8.880 ns |      0.0027 ns |     0.0021 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       143.239 ns |      0.4167 ns |     0.3694 ns |  0.002 |    0.00 |    4 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        15.815 ns |      0.0207 ns |     0.0184 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |               |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    78,924.324 ns |  6,282.3724 ns |   344.3580 ns |  1.000 |    0.01 |    4 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   820,729.974 ns |  9,368.3731 ns |   513.5121 ns | 10.399 |    0.04 |    5 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,382,303.698 ns | 66,262.9136 ns | 3,632.0939 ns | 55.526 |    0.21 |    6 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         8.106 ns |      1.1400 ns |     0.0625 ns |  0.000 |    0.00 |    1 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         7.062 ns |      0.1177 ns |     0.0065 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         8.896 ns |      0.1923 ns |     0.0105 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       138.458 ns |     10.2731 ns |     0.5631 ns |  0.002 |    0.00 |    3 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        15.965 ns |      4.4773 ns |     0.2454 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
