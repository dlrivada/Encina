```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 864.1 ns | 41.06 ns | 27.16 ns |  1.44 |    0.04 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 587.8 ns |  3.22 ns |  1.92 ns |  0.98 |    0.01 |      - |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 598.3 ns |  4.49 ns |  2.97 ns |  1.00 |    0.01 |      - |      40 B |        1.00 |
|                   |            |                |             |          |          |          |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 809.9 ns | 66.42 ns |  3.64 ns |  1.35 |    0.01 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 588.2 ns | 51.76 ns |  2.84 ns |  0.98 |    0.00 |      - |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 598.8 ns | 35.12 ns |  1.93 ns |  1.00 |    0.00 |      - |      40 B |        1.00 |
