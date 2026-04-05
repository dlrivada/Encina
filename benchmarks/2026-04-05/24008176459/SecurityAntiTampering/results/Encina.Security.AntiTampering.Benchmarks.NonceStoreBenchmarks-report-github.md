```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error         | StdDev        | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|--------------:|--------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,203.82 ns |    129.131 ns |     67.538 ns |     1.00 |    0.07 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         56.14 ns |      0.058 ns |      0.035 ns |     0.05 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         22.34 ns |      0.027 ns |      0.016 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |  5,434,198.95 ns | 23,119.742 ns | 15,292.278 ns | 4,526.53 |  238.69 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |              |             |                  |               |               |          |         |        |        |           |             |
| Add         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,233,774.00 ns |            NA |      0.000 ns |     1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| Exists_Hit  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,215,471.00 ns |            NA |      0.000 ns |     0.99 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,082,514.00 ns |            NA |      0.000 ns |     0.95 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,917,709.00 ns |            NA |      0.000 ns |     3.99 |    0.00 |      - |      - |  209656 B |    1,455.94 |
