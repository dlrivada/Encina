```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  5.898 ns |  2.3803 ns | 0.1305 ns |  1.00 |    0.03 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 45.359 ns | 46.2220 ns | 2.5336 ns |  7.69 |    0.40 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 36.908 ns |  0.5838 ns | 0.0320 ns |  6.26 |    0.12 | 0.0018 |     152 B |        6.33 |
