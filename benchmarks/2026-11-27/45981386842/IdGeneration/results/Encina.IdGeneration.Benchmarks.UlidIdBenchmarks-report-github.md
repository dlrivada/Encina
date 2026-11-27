```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 804.4 ns |  5.89 ns | 3.51 ns |  1.34 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 600.0 ns |  3.19 ns | 1.90 ns |  1.00 |      - |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 600.3 ns |  2.91 ns | 1.73 ns |  1.00 |      - |      40 B |        1.00 |
|                   |            |                |             |          |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 800.4 ns | 25.31 ns | 1.39 ns |  1.34 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 581.7 ns | 57.29 ns | 3.14 ns |  0.98 |      - |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 596.2 ns | 12.59 ns | 0.69 ns |  1.00 |      - |      40 B |        1.00 |
