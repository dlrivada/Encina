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
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     | 16           | Default     |     78,321.996 ns |   143.7113 ns |   127.3964 ns |  1.000 |    0.00 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |    819,202.702 ns | 1,813.1521 ns | 1,696.0236 ns | 10.459 |    0.03 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |  4,396,609.975 ns | 6,630.7325 ns | 6,202.3913 ns | 56.135 |    0.12 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     | 16           | Default     |          8.294 ns |     0.0461 ns |     0.0385 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     | 16           | Default     |          7.283 ns |     0.0058 ns |     0.0048 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |          9.005 ns |     0.0018 ns |     0.0014 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     | 16           | Default     |        133.166 ns |     0.6224 ns |     0.5197 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |         16.245 ns |     0.0347 ns |     0.0290 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |              |             |                   |               |               |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 44,270,743.000 ns |            NA |     0.0000 ns |   1.00 |    0.00 |    6 |       - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 45,703,443.000 ns |            NA |     0.0000 ns |   1.03 |    0.00 |    7 |       - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 49,422,330.000 ns |            NA |     0.0000 ns |   1.12 |    0.00 |    8 |       - |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,370,386.000 ns |            NA |     0.0000 ns |   0.08 |    0.00 |    4 |       - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,623,617.000 ns |            NA |     0.0000 ns |   0.04 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,443,512.000 ns |            NA |     0.0000 ns |   0.06 |    0.00 |    3 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,294,451.000 ns |            NA |     0.0000 ns |   0.03 |    0.00 |    1 |       - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,937,336.000 ns |            NA |     0.0000 ns |   0.09 |    0.00 |    5 |       - |         - |       0.000 |
