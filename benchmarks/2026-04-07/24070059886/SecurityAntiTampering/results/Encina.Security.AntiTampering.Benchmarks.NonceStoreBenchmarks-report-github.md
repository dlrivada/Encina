```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error         | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|--------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,237.18 ns |    213.385 ns |   111.604 ns |     1.01 |    0.12 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         56.64 ns |      0.116 ns |     0.061 ns |     0.05 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         22.31 ns |      0.037 ns |     0.022 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |  5,392,772.28 ns | 16,216.333 ns | 8,481.456 ns | 4,387.99 |  348.29 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |              |             |                  |               |              |          |         |        |        |           |             |
| Add         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,529,356.00 ns |            NA |     0.000 ns |     1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| Exists_Hit  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,576,475.00 ns |            NA |     0.000 ns |     1.01 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,827,263.00 ns |            NA |     0.000 ns |     1.08 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,643,437.00 ns |            NA |     0.000 ns |     3.87 |    0.00 |      - |      - |  209656 B |    1,455.94 |
