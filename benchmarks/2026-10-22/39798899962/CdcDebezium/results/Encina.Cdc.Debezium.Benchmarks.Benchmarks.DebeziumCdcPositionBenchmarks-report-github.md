```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.568 ns |  0.4953 ns | 0.0271 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 54.570 ns | 38.8054 ns | 2.1271 ns |  7.21 |    0.24 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 43.377 ns |  5.7864 ns | 0.3172 ns |  5.73 |    0.04 | 0.0091 |     152 B |        6.33 |
