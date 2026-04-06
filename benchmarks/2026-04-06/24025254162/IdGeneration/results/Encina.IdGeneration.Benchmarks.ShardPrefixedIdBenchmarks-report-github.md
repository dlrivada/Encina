```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.234 μs | 0.0041 μs | 0.0027 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.202 μs | 0.0058 μs | 0.0034 μs |  0.97 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.358 μs | 0.0055 μs | 0.0029 μs |  1.10 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.257 μs | 0.0033 μs | 0.0020 μs |  1.02 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.395 μs | 0.0016 μs | 0.0010 μs |  1.13 | 0.0114 |     216 B |        1.80 |
|                                |            |                |             |          |           |           |       |        |           |             |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.245 μs | 0.0384 μs | 0.0021 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.280 μs | 0.1926 μs | 0.0106 μs |  1.03 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.361 μs | 0.1151 μs | 0.0063 μs |  1.09 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.275 μs | 0.0733 μs | 0.0040 μs |  1.02 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.335 μs | 0.0843 μs | 0.0046 μs |  1.07 | 0.0114 |     216 B |        1.80 |
