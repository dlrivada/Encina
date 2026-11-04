```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.393 ns | 2.933 ns | 0.1608 ns |  1.00 |    0.03 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 43.868 ns | 1.636 ns | 0.0897 ns |  6.86 |    0.15 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 36.025 ns | 1.495 ns | 0.0819 ns |  5.64 |    0.12 | 0.0018 |     152 B |        6.33 |
