```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1.263 μs | 0.0040 μs | 0.0024 μs |  1.09 |    0.00 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1.107 μs | 0.0058 μs | 0.0035 μs |  0.96 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 1.154 μs | 0.0044 μs | 0.0026 μs |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |           |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1.236 μs | 0.0307 μs | 0.0017 μs |  1.06 |    0.02 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 1.121 μs | 0.1605 μs | 0.0088 μs |  0.96 |    0.02 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1.167 μs | 0.3521 μs | 0.0193 μs |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
