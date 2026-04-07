```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.476 μs | 0.0076 μs | 0.0040 μs |  1.06 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.444 μs | 0.0085 μs | 0.0056 μs |  1.04 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.393 μs | 0.0017 μs | 0.0010 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.271 μs | 0.0030 μs | 0.0016 μs |  0.91 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.346 μs | 0.0102 μs | 0.0061 μs |  0.97 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.509 μs | 0.0626 μs | 0.0034 μs |  1.11 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.460 μs | 0.0143 μs | 0.0008 μs |  1.08 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.354 μs | 0.0857 μs | 0.0047 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.304 μs | 0.2622 μs | 0.0144 μs |  0.96 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.358 μs | 0.1255 μs | 0.0069 μs |  1.00 | 0.0057 |     120 B |        1.00 |
