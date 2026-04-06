```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate          | Job-YFEFPZ | 10             | Default     | 1.119 μs | 0.0035 μs | 0.0021 μs |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1.194 μs | 0.0076 μs | 0.0045 μs |  1.07 |    0.00 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1.147 μs | 0.0016 μs | 0.0009 μs |  1.03 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |           |       |         |        |           |             |
| Generate          | ShortRun   | 3              | 1           | 1.200 μs | 0.3697 μs | 0.0203 μs |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | ShortRun   | 3              | 1           | 1.198 μs | 0.0454 μs | 0.0025 μs |  1.00 |    0.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 1.075 μs | 0.1297 μs | 0.0071 μs |  0.90 |    0.01 | 0.0019 |      40 B |        1.00 |
