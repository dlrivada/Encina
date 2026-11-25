```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 661.7 ns |  2.74 ns | 1.43 ns |  1.09 | 0.0048 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 580.8 ns |  4.39 ns | 2.91 ns |  0.96 | 0.0010 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 607.3 ns |  6.30 ns | 4.17 ns |  1.00 | 0.0010 |      40 B |        1.00 |
|                   |            |                |             |          |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 657.1 ns | 29.95 ns | 1.64 ns |  1.09 | 0.0048 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 582.4 ns | 51.91 ns | 2.85 ns |  0.97 | 0.0010 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 603.0 ns |  8.18 ns | 0.45 ns |  1.00 | 0.0010 |      40 B |        1.00 |
