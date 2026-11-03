```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.3 ns |   0.06 ns | 0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 662.7 ns |   9.32 ns | 6.16 ns |  2.73 |    0.02 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 654.3 ns |   4.54 ns | 2.70 ns |  2.70 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 841.3 ns |   4.64 ns | 2.42 ns |  3.47 |    0.01 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 678.4 ns |   3.02 ns | 1.80 ns |  2.80 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.3 ns |   0.76 ns | 0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 659.8 ns |  24.59 ns | 1.35 ns |  2.72 |    0.00 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 654.8 ns |  70.00 ns | 3.84 ns |  2.70 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 845.6 ns | 166.40 ns | 9.12 ns |  3.49 |    0.03 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 678.8 ns |   4.78 ns | 0.26 ns |  2.80 |    0.00 |    2 |      - |         - |          NA |
