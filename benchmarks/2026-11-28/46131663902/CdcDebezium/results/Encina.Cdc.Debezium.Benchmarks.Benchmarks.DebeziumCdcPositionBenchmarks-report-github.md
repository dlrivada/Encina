```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  5.728 ns | 1.5638 ns | 0.0857 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 44.434 ns | 0.6470 ns | 0.0355 ns |  7.76 |    0.10 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 34.083 ns | 9.3990 ns | 0.5152 ns |  5.95 |    0.11 | 0.0018 |     152 B |        6.33 |
