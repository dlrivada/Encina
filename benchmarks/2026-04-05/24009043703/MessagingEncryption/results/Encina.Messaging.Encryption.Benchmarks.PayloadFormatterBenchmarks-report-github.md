```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean              | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |------------- |------------ |------------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Format      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       338.6456 ns | 3.4206 ns | 2.2625 ns | 1.000 |    0.01 | 0.1016 |    1704 B |        1.00 |
| TryParse    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     1,128.9285 ns | 5.2804 ns | 3.4927 ns | 3.334 |    0.02 | 0.1106 |    1872 B |        1.10 |
| IsEncrypted | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         0.4925 ns | 0.0121 ns | 0.0072 ns | 0.001 |    0.00 |      - |         - |        0.00 |
| Roundtrip   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     1,506.4018 ns | 7.2303 ns | 4.7824 ns | 4.448 |    0.03 | 0.2136 |    3576 B |        2.10 |
|             |            |                |             |             |              |             |                   |           |           |       |         |        |           |             |
| Format      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   249,117.0000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |      - |    1704 B |        1.00 |
| TryParse    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,079,162.0000 ns |        NA | 0.0000 ns |  4.33 |    0.00 |      - |    1872 B |        1.10 |
| IsEncrypted | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   335,539.0000 ns |        NA | 0.0000 ns |  1.35 |    0.00 |      - |         - |        0.00 |
| Roundtrip   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,086,475.0000 ns |        NA | 0.0000 ns |  4.36 |    0.00 |      - |    3576 B |        2.10 |
