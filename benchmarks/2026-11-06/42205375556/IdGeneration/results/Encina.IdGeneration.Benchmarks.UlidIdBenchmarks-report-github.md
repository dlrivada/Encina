```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 962.9 ns |  25.43 ns | 16.82 ns |  1.14 |    0.02 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 830.4 ns |   6.78 ns |  4.04 ns |  0.98 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 848.2 ns |   4.94 ns |  2.94 ns |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |          |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 934.1 ns | 133.59 ns |  7.32 ns |  1.03 |    0.01 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 871.5 ns | 119.45 ns |  6.55 ns |  0.96 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 905.9 ns | 214.45 ns | 11.75 ns |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
