```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.579 ns | 2.101 ns | 0.1152 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 64.844 ns | 3.723 ns | 0.2041 ns |  7.56 |    0.09 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 49.003 ns | 1.716 ns | 0.0941 ns |  5.71 |    0.07 | 0.0018 |     152 B |        6.33 |
