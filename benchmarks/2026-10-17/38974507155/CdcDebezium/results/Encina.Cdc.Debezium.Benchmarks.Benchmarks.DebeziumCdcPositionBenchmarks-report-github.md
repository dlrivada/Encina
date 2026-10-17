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
| CreatePosition |  7.709 ns |  0.8410 ns | 0.0461 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 54.488 ns | 13.7609 ns | 0.7543 ns |  7.07 |    0.09 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 42.046 ns |  6.1078 ns | 0.3348 ns |  5.45 |    0.05 | 0.0091 |     152 B |        6.33 |
