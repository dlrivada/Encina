```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.735 ns | 0.5672 ns | 0.0311 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 45.066 ns | 4.7297 ns | 0.2592 ns |  6.69 |    0.04 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 40.397 ns | 4.1409 ns | 0.2270 ns |  6.00 |    0.04 | 0.0018 |     152 B |        6.33 |
