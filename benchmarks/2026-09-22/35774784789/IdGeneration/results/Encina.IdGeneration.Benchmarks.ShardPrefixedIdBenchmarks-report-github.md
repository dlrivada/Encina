```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     |   969.2 ns |     5.85 ns |  3.87 ns |  0.94 |    0.01 |      - |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,090.8 ns |    41.50 ns | 24.70 ns |  1.06 |    0.02 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1,031.6 ns |    12.90 ns |  7.68 ns |  1.00 |    0.01 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     |   884.6 ns |     3.66 ns |  1.91 ns |  0.86 |    0.01 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1,052.1 ns |     8.82 ns |  4.61 ns |  1.02 |    0.01 |      - |     120 B |        1.00 |
|                                |            |                |             |            |             |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           |   983.2 ns |    26.87 ns |  1.47 ns |  0.95 |    0.00 |      - |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1,124.8 ns |   575.50 ns | 31.55 ns |  1.09 |    0.03 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1,029.9 ns |    55.76 ns |  3.06 ns |  1.00 |    0.00 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   892.6 ns |    55.80 ns |  3.06 ns |  0.87 |    0.00 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1,074.1 ns | 1,172.06 ns | 64.24 ns |  1.04 |    0.05 |      - |     120 B |        1.00 |
