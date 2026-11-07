```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 730.6 ns |   6.65 ns | 3.96 ns |  1.09 |    0.01 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 652.9 ns |  10.84 ns | 6.45 ns |  0.98 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 668.8 ns |   9.33 ns | 5.55 ns |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |         |       |         |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 758.0 ns |  72.86 ns | 3.99 ns |  1.14 |    0.01 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 680.7 ns |  22.77 ns | 1.25 ns |  1.03 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 662.9 ns | 151.58 ns | 8.31 ns |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |
