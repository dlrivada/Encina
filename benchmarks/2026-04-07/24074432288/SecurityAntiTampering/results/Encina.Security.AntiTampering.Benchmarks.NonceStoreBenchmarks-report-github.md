```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-YFEFPZ | 10             | Default     | 3           |     1,240.37 ns |    210.595 ns |    110.145 ns |     1.01 |    0.12 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     | 3           |        55.99 ns |      0.126 ns |      0.083 ns |     0.05 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     | 3           |        22.30 ns |      0.029 ns |      0.015 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | 3           | 5,429,209.98 ns | 24,707.327 ns | 16,342.367 ns | 4,405.63 |  346.96 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |               |          |         |        |        |           |             |
| Add         | MediumRun  | 15             | 2           | 10          |     1,276.85 ns |     30.436 ns |     39.575 ns |     1.00 |    0.04 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | MediumRun  | 15             | 2           | 10          |        55.99 ns |      0.102 ns |      0.136 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | MediumRun  | 15             | 2           | 10          |        22.29 ns |      0.051 ns |      0.071 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | MediumRun  | 15             | 2           | 10          | 5,397,255.96 ns | 11,761.262 ns | 17,603.705 ns | 4,231.01 |  132.25 | 7.8125 |      - |  209656 B |    1,455.94 |
