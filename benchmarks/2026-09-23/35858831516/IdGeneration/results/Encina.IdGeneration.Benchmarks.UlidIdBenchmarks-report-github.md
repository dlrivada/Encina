```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,017.7 ns |   2.10 ns | 1.39 ns |  1.07 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   916.1 ns |   3.05 ns | 2.02 ns |  0.96 | 0.0010 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   954.1 ns |   2.39 ns | 1.42 ns |  1.00 |      - |      40 B |        1.00 |
|                   |            |                |             |            |           |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,033.1 ns |  54.45 ns | 2.98 ns |  1.07 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   920.8 ns | 113.97 ns | 6.25 ns |  0.96 | 0.0010 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   961.7 ns |  39.93 ns | 2.19 ns |  1.00 |      - |      40 B |        1.00 |
