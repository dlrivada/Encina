```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1.176 μs | 0.0095 μs | 0.0063 μs |  1.06 |    0.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1.055 μs | 0.0026 μs | 0.0014 μs |  0.96 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 1.105 μs | 0.0034 μs | 0.0020 μs |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |           |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1.190 μs | 0.1084 μs | 0.0059 μs |  1.01 |    0.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 1.061 μs | 0.0746 μs | 0.0041 μs |  0.90 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1.181 μs | 0.2863 μs | 0.0157 μs |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
