```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate          | Job-YFEFPZ | 10             | Default     | 3           | 1.177 μs | 0.0056 μs | 0.0037 μs |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 1.253 μs | 0.0049 μs | 0.0025 μs |  1.06 |    0.00 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           | 1.072 μs | 0.0067 μs | 0.0045 μs |  0.91 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |          |           |           |       |         |        |           |             |
| Generate          | MediumRun  | 15             | 2           | 10          | 1.098 μs | 0.0016 μs | 0.0022 μs |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 1.219 μs | 0.0267 μs | 0.0400 μs |  1.11 |    0.04 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          | 1.063 μs | 0.0023 μs | 0.0034 μs |  0.97 |    0.00 | 0.0019 |      40 B |        1.00 |
