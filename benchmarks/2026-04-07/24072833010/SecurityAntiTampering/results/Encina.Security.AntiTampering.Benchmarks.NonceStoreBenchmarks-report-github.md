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
| Add         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,277.75 ns |    199.046 ns |    104.105 ns |     1.01 |    0.11 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         56.42 ns |      0.351 ns |      0.232 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         22.26 ns |      0.024 ns |      0.014 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |  5,425,718.36 ns | 27,808.265 ns | 18,393.445 ns | 4,270.00 |  314.14 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |              |             |                  |               |               |          |         |        |        |           |             |
| Add         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,404,567.00 ns |            NA |      0.000 ns |     1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| Exists_Hit  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,343,062.00 ns |            NA |      0.000 ns |     0.98 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,263,584.00 ns |            NA |      0.000 ns |     0.96 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,230,915.00 ns |            NA |      0.000 ns |     3.89 |    0.00 |      - |      - |  209656 B |    1,455.94 |
