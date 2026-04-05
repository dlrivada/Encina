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
| Format      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       382.3588 ns | 6.7402 ns | 4.0110 ns | 1.000 |    0.01 | 0.1016 |    1704 B |        1.00 |
| TryParse    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     1,180.7004 ns | 3.0489 ns | 1.8144 ns | 3.088 |    0.03 | 0.1106 |    1872 B |        1.10 |
| IsEncrypted | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         0.5311 ns | 0.0661 ns | 0.0437 ns | 0.001 |    0.00 |      - |         - |        0.00 |
| Roundtrip   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     1,610.7995 ns | 5.6764 ns | 3.3780 ns | 4.213 |    0.04 | 0.2136 |    3576 B |        2.10 |
|             |            |                |             |             |              |             |                   |           |           |       |         |        |           |             |
| Format      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   273,534.0000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |      - |    1704 B |        1.00 |
| TryParse    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,123,271.0000 ns |        NA | 0.0000 ns |  4.11 |    0.00 |      - |    1872 B |        1.10 |
| IsEncrypted | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   356,921.0000 ns |        NA | 0.0000 ns |  1.30 |    0.00 |      - |         - |        0.00 |
| Roundtrip   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,161,392.0000 ns |        NA | 0.0000 ns |  4.25 |    0.00 |      - |    3576 B |        2.10 |
