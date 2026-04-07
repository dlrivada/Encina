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
| Generate          | Job-YFEFPZ | 10             | Default     | 3           | 1.176 μs | 0.0095 μs | 0.0063 μs |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 1.186 μs | 0.0037 μs | 0.0022 μs |  1.01 |    0.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           | 1.062 μs | 0.0056 μs | 0.0033 μs |  0.90 |    0.01 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |          |           |           |       |         |        |           |             |
| Generate          | MediumRun  | 15             | 2           | 10          | 1.098 μs | 0.0029 μs | 0.0042 μs |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 1.255 μs | 0.0018 μs | 0.0027 μs |  1.14 |    0.00 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          | 1.098 μs | 0.0202 μs | 0.0303 μs |  1.00 |    0.03 | 0.0019 |      40 B |        1.00 |
