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
| CreatePosition |  8.410 ns |  0.6769 ns | 0.0371 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 63.669 ns | 50.6427 ns | 2.7759 ns |  7.57 |    0.29 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 45.507 ns |  6.0286 ns | 0.3304 ns |  5.41 |    0.04 | 0.0091 |     152 B |        6.33 |
