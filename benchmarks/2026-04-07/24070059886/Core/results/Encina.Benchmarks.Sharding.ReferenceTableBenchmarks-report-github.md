```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean              | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |------------------:|--------------:|--------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     | 16           | Default     |     82,214.247 ns |   132.9868 ns |   124.3960 ns |  1.000 |    0.00 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |    863,017.897 ns | 1,047.5662 ns |   874.7652 ns | 10.497 |    0.02 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |  4,404,922.925 ns | 8,740.6438 ns | 7,748.3530 ns | 53.579 |    0.12 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     | 16           | Default     |          8.308 ns |     0.0581 ns |     0.0515 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     | 16           | Default     |          7.281 ns |     0.0054 ns |     0.0045 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |          9.009 ns |     0.0045 ns |     0.0040 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     | 16           | Default     |        134.032 ns |     1.2383 ns |     1.0977 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |         16.243 ns |     0.0082 ns |     0.0064 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |              |             |                   |               |               |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 44,906,256.000 ns |            NA |     0.0000 ns |   1.00 |    0.00 |    6 |       - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 45,596,483.000 ns |            NA |     0.0000 ns |   1.02 |    0.00 |    7 |       - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50,363,767.000 ns |            NA |     0.0000 ns |   1.12 |    0.00 |    8 |       - |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,448,684.000 ns |            NA |     0.0000 ns |   0.08 |    0.00 |    4 |       - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,673,231.000 ns |            NA |     0.0000 ns |   0.04 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,439,321.000 ns |            NA |     0.0000 ns |   0.05 |    0.00 |    3 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,793,566.000 ns |            NA |     0.0000 ns |   0.04 |    0.00 |    2 |       - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,035,058.000 ns |            NA |     0.0000 ns |   0.09 |    0.00 |    5 |       - |         - |       0.000 |
