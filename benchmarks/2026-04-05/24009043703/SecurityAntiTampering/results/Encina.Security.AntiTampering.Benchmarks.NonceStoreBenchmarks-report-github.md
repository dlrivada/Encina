```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error         | StdDev        | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|--------------:|--------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        972.30 ns |    107.074 ns |     56.002 ns |     1.00 |    0.08 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         49.71 ns |      0.101 ns |      0.060 ns |     0.05 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         21.08 ns |      0.057 ns |      0.034 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |  5,345,704.47 ns | 25,553.027 ns | 16,901.745 ns | 5,514.13 |  302.67 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |              |             |                  |               |               |          |         |        |        |           |             |
| Add         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,141,504.00 ns |            NA |      0.000 ns |     1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| Exists_Hit  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,127,577.00 ns |            NA |      0.000 ns |     1.00 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,999,964.00 ns |            NA |      0.000 ns |     0.95 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,702,223.00 ns |            NA |      0.000 ns |     4.04 |    0.00 |      - |      - |  209656 B |    1,455.94 |
