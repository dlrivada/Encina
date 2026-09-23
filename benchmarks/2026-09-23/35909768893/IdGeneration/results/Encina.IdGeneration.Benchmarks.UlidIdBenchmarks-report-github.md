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
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1.296 μs | 0.0029 μs | 0.0015 μs |  1.14 |    0.02 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1.107 μs | 0.0102 μs | 0.0061 μs |  0.98 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 1.132 μs | 0.0247 μs | 0.0163 μs |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |           |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1.221 μs | 0.0712 μs | 0.0039 μs |  1.08 |    0.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 1.198 μs | 0.3988 μs | 0.0219 μs |  1.06 |    0.02 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1.134 μs | 0.1752 μs | 0.0096 μs |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
