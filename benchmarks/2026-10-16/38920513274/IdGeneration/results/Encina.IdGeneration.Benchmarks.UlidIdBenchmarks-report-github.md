```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.23GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,032.8 ns |  7.33 ns | 4.85 ns |  1.07 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   953.7 ns |  3.63 ns | 2.40 ns |  0.99 |      - |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   968.1 ns |  2.48 ns | 1.47 ns |  1.00 |      - |      40 B |        1.00 |
|                   |            |                |             |            |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,031.8 ns | 45.76 ns | 2.51 ns |  1.06 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   951.2 ns | 55.78 ns | 3.06 ns |  0.98 |      - |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   970.9 ns | 11.08 ns | 0.61 ns |  1.00 |      - |      40 B |        1.00 |
