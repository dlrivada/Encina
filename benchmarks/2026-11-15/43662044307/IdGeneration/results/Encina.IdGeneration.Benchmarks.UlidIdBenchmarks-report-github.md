```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.76GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 865.5 ns |  4.92 ns |  3.26 ns | 865.6 ns |  1.11 |    0.00 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           | 758.5 ns |  4.73 ns |  3.13 ns | 757.1 ns |  0.97 |    0.00 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           | 780.1 ns |  3.26 ns |  1.94 ns | 780.0 ns |  1.00 |    0.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |          |          |          |          |       |         |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 884.4 ns |  9.14 ns | 13.11 ns | 884.7 ns |  1.09 |    0.03 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          | 768.4 ns | 12.19 ns | 16.27 ns | 781.6 ns |  0.95 |    0.03 | 0.0019 |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          | 810.9 ns | 13.25 ns | 19.01 ns | 811.2 ns |  1.00 |    0.03 | 0.0019 |      40 B |        1.00 |
