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
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     | 16           | Default     |     80,992.686 ns |   340.6362 ns |   318.6313 ns |  1.000 |    0.01 |    5 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |    830,923.607 ns |   501.9860 ns |   444.9975 ns | 10.259 |    0.04 |    6 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |  4,476,806.878 ns | 6,820.5266 ns | 5,325.0185 ns | 55.275 |    0.22 |    7 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     | 16           | Default     |          8.680 ns |     0.1842 ns |     0.1723 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     | 16           | Default     |          7.285 ns |     0.0085 ns |     0.0071 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |          9.007 ns |     0.0097 ns |     0.0091 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     | 16           | Default     |        141.551 ns |     0.6623 ns |     0.5871 ns |  0.002 |    0.00 |    4 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |         16.326 ns |     0.1750 ns |     0.1551 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
|                                            |            |                |             |             |              |             |                   |               |               |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 45,475,929.000 ns |            NA |     0.0000 ns |   1.00 |    0.00 |    6 |       - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 45,753,154.000 ns |            NA |     0.0000 ns |   1.01 |    0.00 |    7 |       - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 51,282,683.000 ns |            NA |     0.0000 ns |   1.13 |    0.00 |    8 |       - |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,440,549.000 ns |            NA |     0.0000 ns |   0.08 |    0.00 |    3 |       - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,730,228.000 ns |            NA |     0.0000 ns |   0.04 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,599,205.000 ns |            NA |     0.0000 ns |   0.08 |    0.00 |    4 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,384,666.000 ns |            NA |     0.0000 ns |   0.03 |    0.00 |    1 |       - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,119,963.000 ns |            NA |     0.0000 ns |   0.09 |    0.00 |    5 |       - |         - |       0.000 |
