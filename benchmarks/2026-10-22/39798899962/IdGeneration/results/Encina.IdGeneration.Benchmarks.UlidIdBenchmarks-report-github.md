```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1.196 μs | 0.0019 μs | 0.0011 μs |  1.05 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1.158 μs | 0.0041 μs | 0.0024 μs |  1.01 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 1.142 μs | 0.0033 μs | 0.0020 μs |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |           |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1.184 μs | 0.0582 μs | 0.0032 μs |  1.05 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 1.098 μs | 0.1421 μs | 0.0078 μs |  0.98 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1.123 μs | 0.0818 μs | 0.0045 μs |  1.00 | 0.0019 |      40 B |        1.00 |
