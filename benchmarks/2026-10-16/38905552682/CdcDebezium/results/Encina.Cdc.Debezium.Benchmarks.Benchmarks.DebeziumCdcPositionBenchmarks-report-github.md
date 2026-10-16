```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.672 ns | 2.601 ns | 0.1426 ns |  1.00 |    0.03 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 46.488 ns | 9.551 ns | 0.5235 ns |  6.97 |    0.14 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 35.782 ns | 1.144 ns | 0.0627 ns |  5.36 |    0.10 | 0.0018 |     152 B |        6.33 |
