```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1.181 μs | 0.0032 μs | 0.0019 μs |  1.06 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 1.159 μs | 0.0039 μs | 0.0026 μs |  1.04 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 1.118 μs | 0.0083 μs | 0.0055 μs |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |           |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1.182 μs | 0.0684 μs | 0.0037 μs |  1.01 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 1.066 μs | 0.0741 μs | 0.0041 μs |  0.91 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1.170 μs | 0.1220 μs | 0.0067 μs |  1.00 | 0.0019 |      40 B |        1.00 |
