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
| CreatePosition |  8.188 ns |  1.228 ns | 0.0673 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 59.043 ns | 28.121 ns | 1.5414 ns |  7.21 |    0.17 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.918 ns |  9.886 ns | 0.5419 ns |  5.49 |    0.07 | 0.0091 |     152 B |        6.33 |
