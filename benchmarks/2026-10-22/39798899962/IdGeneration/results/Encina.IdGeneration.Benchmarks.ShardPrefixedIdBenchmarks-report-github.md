```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.432 μs | 0.0026 μs | 0.0013 μs |  1.04 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.400 μs | 0.0052 μs | 0.0034 μs |  1.01 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.379 μs | 0.0031 μs | 0.0018 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.356 μs | 0.0029 μs | 0.0017 μs |  0.98 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.406 μs | 0.0024 μs | 0.0016 μs |  1.02 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.512 μs | 0.0268 μs | 0.0015 μs |  1.17 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.476 μs | 0.0447 μs | 0.0025 μs |  1.14 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.295 μs | 0.0510 μs | 0.0028 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.307 μs | 0.1420 μs | 0.0078 μs |  1.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.338 μs | 0.0531 μs | 0.0029 μs |  1.03 | 0.0057 |     120 B |        1.00 |
