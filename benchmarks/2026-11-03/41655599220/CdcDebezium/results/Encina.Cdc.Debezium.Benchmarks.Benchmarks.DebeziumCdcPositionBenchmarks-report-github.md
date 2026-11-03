```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.515 ns | 0.3398 ns | 0.0186 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 61.029 ns | 0.7180 ns | 0.0394 ns |  7.17 |    0.01 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 46.165 ns | 3.2693 ns | 0.1792 ns |  5.42 |    0.02 | 0.0091 |     152 B |        6.33 |
