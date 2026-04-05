```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.74GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean              | Error          | StdDev         | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |------------------:|---------------:|---------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     | 16           | Default     |     79,001.999 ns |    244.6770 ns |    228.8710 ns |  1.000 |    0.00 |    6 |  0.4883 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |    827,526.942 ns |  2,372.3330 ns |  2,103.0114 ns | 10.475 |    0.04 |    7 |  4.8828 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |  4,485,647.022 ns | 15,461.3169 ns | 14,462.5254 ns | 56.779 |    0.24 |    8 | 23.4375 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     | 16           | Default     |          8.419 ns |      0.1007 ns |      0.0942 ns |  0.000 |    0.00 |    3 |  0.0010 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     | 16           | Default     |          6.699 ns |      0.0053 ns |      0.0050 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |          7.991 ns |      0.0093 ns |      0.0083 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     | 16           | Default     |        127.917 ns |      0.5951 ns |      0.4969 ns |  0.002 |    0.00 |    5 |  0.0043 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |         16.018 ns |      0.0295 ns |      0.0262 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |              |             |                   |                |                |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 41,750,427.000 ns |             NA |      0.0000 ns |   1.00 |    0.00 |    6 |       - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 42,669,542.000 ns |             NA |      0.0000 ns |   1.02 |    0.00 |    7 |       - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 46,429,852.000 ns |             NA |      0.0000 ns |   1.11 |    0.00 |    8 |       - |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,104,378.000 ns |             NA |      0.0000 ns |   0.07 |    0.00 |    4 |       - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,569,690.000 ns |             NA |      0.0000 ns |   0.04 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,292,210.000 ns |             NA |      0.0000 ns |   0.05 |    0.00 |    3 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,210,860.000 ns |             NA |      0.0000 ns |   0.03 |    0.00 |    1 |       - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,831,921.000 ns |             NA |      0.0000 ns |   0.09 |    0.00 |    5 |       - |         - |       0.000 |
