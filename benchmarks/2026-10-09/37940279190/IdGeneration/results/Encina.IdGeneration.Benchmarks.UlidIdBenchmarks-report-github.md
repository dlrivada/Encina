```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1.270 μs | 0.0093 μs | 0.0055 μs |  1.07 |    0.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1.165 μs | 0.0140 μs | 0.0093 μs |  0.98 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 1.189 μs | 0.0063 μs | 0.0041 μs |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |           |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1.321 μs | 0.4621 μs | 0.0253 μs |  1.12 |    0.02 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 1.169 μs | 0.2439 μs | 0.0134 μs |  0.99 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1.180 μs | 0.1247 μs | 0.0068 μs |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
