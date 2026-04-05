```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean              | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |------------- |------------ |------------------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Format      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       350.9539 ns |  4.8628 ns |  2.8938 ns | 1.000 |    0.01 | 0.1016 |    1704 B |        1.00 |
| TryParse    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     1,150.8520 ns | 14.9510 ns |  9.8891 ns | 3.279 |    0.04 | 0.1106 |    1872 B |        1.10 |
| IsEncrypted | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         0.5061 ns |  0.0236 ns |  0.0156 ns | 0.001 |    0.00 |      - |         - |        0.00 |
| Roundtrip   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     1,588.3337 ns | 18.4685 ns | 10.9903 ns | 4.526 |    0.05 | 0.2136 |    3576 B |        2.10 |
|             |            |                |             |             |              |             |                   |            |            |       |         |        |           |             |
| Format      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   270,351.0000 ns |         NA |  0.0000 ns |  1.00 |    0.00 |      - |    1704 B |        1.00 |
| TryParse    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,162,574.0000 ns |         NA |  0.0000 ns |  4.30 |    0.00 |      - |    1872 B |        1.10 |
| IsEncrypted | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   360,748.0000 ns |         NA |  0.0000 ns |  1.33 |    0.00 |      - |         - |        0.00 |
| Roundtrip   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,161,160.0000 ns |         NA |  0.0000 ns |  4.30 |    0.00 |      - |    3576 B |        2.10 |
