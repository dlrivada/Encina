```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.34GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,109.9 ns |   8.91 ns |  5.90 ns |  1.03 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1,061.8 ns |   7.32 ns |  4.35 ns |  0.99 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 1,076.5 ns |  13.78 ns |  9.11 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,089.5 ns |  52.90 ns |  2.90 ns |  1.06 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   986.0 ns | 230.01 ns | 12.61 ns |  0.96 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1,028.8 ns | 196.30 ns | 10.76 ns |  1.00 | 0.0019 |      40 B |        1.00 |
