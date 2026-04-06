```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean              | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |------------- |------------ |------------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Format      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       388.6432 ns |  4.1617 ns | 2.4766 ns | 1.000 |    0.01 | 0.1016 |    1704 B |        1.00 |
| TryParse    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     1,207.9838 ns |  9.6054 ns | 6.3534 ns | 3.108 |    0.02 | 0.1106 |    1872 B |        1.10 |
| IsEncrypted | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         0.5242 ns |  0.0212 ns | 0.0140 ns | 0.001 |    0.00 |      - |         - |        0.00 |
| Roundtrip   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     1,636.6077 ns | 14.2246 ns | 7.4397 ns | 4.211 |    0.03 | 0.2136 |    3576 B |        2.10 |
|             |            |                |             |             |              |             |                   |            |           |       |         |        |           |             |
| Format      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   297,327.0000 ns |         NA | 0.0000 ns |  1.00 |    0.00 |      - |    1704 B |        1.00 |
| TryParse    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,194,979.0000 ns |         NA | 0.0000 ns |  4.02 |    0.00 |      - |    1872 B |        1.10 |
| IsEncrypted | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   359,864.0000 ns |         NA | 0.0000 ns |  1.21 |    0.00 |      - |         - |        0.00 |
| Roundtrip   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,157,449.0000 ns |         NA | 0.0000 ns |  3.89 |    0.00 |      - |    3576 B |        2.10 |
