```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.609 ns |  1.391 ns | 0.0762 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 60.159 ns | 14.517 ns | 0.7957 ns |  7.91 |    0.11 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 49.218 ns |  7.042 ns | 0.3860 ns |  6.47 |    0.07 | 0.0018 |     152 B |        6.33 |
