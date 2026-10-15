```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.821 ns |  1.234 ns | 0.0676 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 55.610 ns | 28.440 ns | 1.5589 ns |  7.11 |    0.18 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.118 ns | 34.552 ns | 1.8939 ns |  5.64 |    0.21 | 0.0091 |     152 B |        6.33 |
