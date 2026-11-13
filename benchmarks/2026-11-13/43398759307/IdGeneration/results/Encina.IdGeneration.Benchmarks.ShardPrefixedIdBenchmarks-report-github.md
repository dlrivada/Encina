```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1,028.7 ns |   9.66 ns | 5.75 ns |  1.13 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,021.7 ns |   3.69 ns | 2.19 ns |  1.12 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     |   913.3 ns |   2.64 ns | 1.57 ns |  1.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     |   876.0 ns |   4.55 ns | 3.01 ns |  0.96 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     |   936.9 ns |   5.13 ns | 3.39 ns |  1.03 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |            |           |         |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1,061.4 ns | 172.74 ns | 9.47 ns |  1.16 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1,014.6 ns | 165.96 ns | 9.10 ns |  1.10 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           |   918.9 ns |  46.48 ns | 2.55 ns |  1.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   921.0 ns |  74.81 ns | 4.10 ns |  1.00 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           |   932.2 ns |  72.37 ns | 3.97 ns |  1.01 | 0.0067 |     120 B |        1.00 |
