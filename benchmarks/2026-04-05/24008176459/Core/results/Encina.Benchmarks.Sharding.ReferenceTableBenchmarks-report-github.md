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
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     | 16           | Default     |     79,530.149 ns |   192.8020 ns |   180.3471 ns |  1.000 |    0.00 |    5 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |    825,714.445 ns | 1,209.1656 ns | 1,071.8938 ns | 10.382 |    0.03 |    6 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 16           | Default     |  4,375,081.726 ns | 8,280.2362 ns | 7,745.3381 ns | 55.012 |    0.15 |    7 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     | 16           | Default     |          8.756 ns |     0.0655 ns |     0.0580 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     | 16           | Default     |          7.282 ns |     0.0047 ns |     0.0039 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |          9.012 ns |     0.0073 ns |     0.0057 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     | 16           | Default     |        140.186 ns |     0.3877 ns |     0.3437 ns |  0.002 |    0.00 |    4 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |         16.241 ns |     0.0138 ns |     0.0108 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
|                                            |            |                |             |             |              |             |                   |               |               |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 48,627,119.000 ns |            NA |     0.0000 ns |   1.00 |    0.00 |    7 |       - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 47,922,235.000 ns |            NA |     0.0000 ns |   0.99 |    0.00 |    6 |       - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 51,130,542.000 ns |            NA |     0.0000 ns |   1.05 |    0.00 |    8 |       - |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,513,601.000 ns |            NA |     0.0000 ns |   0.07 |    0.00 |    4 |       - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,821,923.000 ns |            NA |     0.0000 ns |   0.04 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,543,799.000 ns |            NA |     0.0000 ns |   0.05 |    0.00 |    3 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,345,839.000 ns |            NA |     0.0000 ns |   0.03 |    0.00 |    1 |       - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,132,924.000 ns |            NA |     0.0000 ns |   0.08 |    0.00 |    5 |       - |         - |       0.000 |
