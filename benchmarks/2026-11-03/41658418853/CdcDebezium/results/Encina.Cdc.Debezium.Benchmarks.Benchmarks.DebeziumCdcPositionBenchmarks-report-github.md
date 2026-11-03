```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.058 ns |  1.853 ns | 0.1016 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 39.683 ns | 33.537 ns | 1.8383 ns |  5.62 |    0.24 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 36.700 ns | 26.352 ns | 1.4444 ns |  5.20 |    0.19 | 0.0018 |     152 B |        6.33 |
